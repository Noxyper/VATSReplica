using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Cinemachine;

struct IndicatorLimbPairing
{
    public Indicator indicator;
    public Limbs limb;
}

public class VATS : MonoBehaviour
{
    PlayerShoot _shootClass;
    PlayerMove _moveClass;
    CharacterStats _playerStats;
    List<EnemyMove> _enemies;
    List<EnemyMove> _enemiesInViewport;
    List<EnemyMove> _enemiesVisible;
    List<IndicatorLimbPairing> _indicators;
    Stack<Limbs> _VATSSetupOrder;
    Queue<Limbs> _VATSShootingOrder;
    Color _sliderBaseColor;

    bool _inAnimation;
    bool _animationPlaying;
    bool _VATSactive;
    bool _justEnteredVATS;
    bool _mouseHoverMode;
    bool _flashFlip = true;
    float _zoomLerpElapsed = 0f;
    float _zoomChangeLerpElapsed = 0f;
    float _moveLerpElapsed = 0f;
    float _previousFOV = 60;
    float _newFOV;
    float _currentVATSCost;
    float _fixedTimeStepBase;
    float _flashTimeStep;
    float _flashTimeStepMax = 2f;
    int _selectedEnemy;
    int _previousEnemy;
    int _currentLimb;

    public Camera VATSCamera;
    public Camera VATSCinemaCameraBrain;
    public CinemachineVirtualCamera VATSCinemaCamera;
    public GameObject VATSHeadsUpDisplay;
    public RectTransform VATSHitChanceIndicators;
    public GameObject VATSIndicator;
    public RectTransform VATSIndicatorSelector;
    public Slider VATSEnemyHealthBar;
    public Text VATSEnemyHealthName;
    public RectTransform APIndicatorParent;
    public GameObject APIndicator;
    [Space]
    public Slider playerHealthBar;
    public Slider playerActionBar;
    [Space]
    public float lerpModifier = 2f;
    public float VATSRange = 100f;
    public float VATSFocusFOV = 20f;
    public float VATSHitChanceModifier = 1f;
    public float VATSCost = 25f;
    public float VATSHangTime = 1f;
    public float APFlashLength = 0.5f;

    void Awake()
    {
        _moveClass = GetComponent<PlayerMove>();
        _shootClass = GetComponent<PlayerShoot>();
        _playerStats = GetComponent<CharacterStats>();

        _enemies = FindObjectsOfType<EnemyMove>().ToList();
        _enemiesInViewport = new List<EnemyMove>();
        _enemiesVisible = new List<EnemyMove>();
        _indicators = new List<IndicatorLimbPairing>();
        _VATSSetupOrder = new Stack<Limbs>();
        _VATSShootingOrder = new Queue<Limbs>();

        playerHealthBar.maxValue = _playerStats.maxHealthPoints;
        playerActionBar.maxValue = _playerStats.maxActionPoints;

        _fixedTimeStepBase = Time.fixedDeltaTime;

        _sliderBaseColor = playerActionBar.fillRect.GetComponent<Image>().color;

        _previousEnemy = _selectedEnemy;
    }

    void Update()
    {
        playerHealthBar.value = _playerStats.healthPoints;
        playerActionBar.value = _playerStats.actionPoints;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        if(_enemies.Any(x => x.GetComponent<CharacterStats>().healthPoints <= 0))
        {
            EnemyMove deleteEnemy = _enemies.Where(x => x.GetComponent<CharacterStats>().healthPoints <= 0).First();
            _enemies.Remove(deleteEnemy);
            if (_enemiesInViewport.Contains(deleteEnemy))
            {
                _enemiesInViewport.Remove(deleteEnemy);
                if (_enemiesVisible.Contains(deleteEnemy))
                {
                    _enemiesVisible.Remove(deleteEnemy);
                }
            }
        }

        foreach (EnemyMove enemy in _enemies)
        {
            if (GeometryUtility.TestPlanesAABB(planes, enemy.GetComponent<CharacterController>().bounds) && !_enemiesInViewport.Contains(enemy) && !enemy.isDead)
            {
                _enemiesInViewport.Add(enemy);
            }
            else if((!GeometryUtility.TestPlanesAABB(planes, enemy.GetComponent<CharacterController>().bounds) && _enemiesInViewport.Contains(enemy)) || enemy.isDead)
            {
                _enemiesInViewport.Remove(enemy);
            }
            
        }

        if (Input.GetButtonDown("Enter VATS") && _enemiesInViewport.Count > 0 && !_VATSactive) _justEnteredVATS = _VATSactive = true;
        if (Input.GetButtonDown("Exit VATS") && !_VATSSetupOrder.Any() && !_inAnimation) _VATSactive = false;

        VATSCamera.gameObject.SetActive(_VATSactive);

        switch (_VATSactive)
        {
            case true:
                Camera.main.depth = -99;

                if (!_inAnimation)
                {
                    VATSEnemyHealthName.transform.parent.gameObject.SetActive(true);
                    RaycastHit[] tempHit = new RaycastHit[_enemiesInViewport.Count];
                    for (int i = 0; i < _enemiesInViewport.Count; i++)
                    {
                        Vector3 origin = _shootClass.weaponProjectilePos.position;
                        Vector3 direction = ((_enemiesInViewport[i].transform.position + (Vector3.up * (_enemiesInViewport[i].GetComponent<CharacterController>().height / 2))) - _shootClass.weaponProjectilePos.position).normalized * VATSRange;

                        if (Physics.Raycast(origin, direction, out tempHit[i], Mathf.Min(VATSRange, Vector3.Distance(_enemiesInViewport[i].transform.position, _shootClass.weaponProjectilePos.position))))
                        {
                            if (tempHit[i].collider == _enemiesInViewport[i].GetComponent<CapsuleCollider>())
                            {
                                Cursor.visible = true;
                                Cursor.lockState = CursorLockMode.None;

                                if (!_enemiesVisible.Contains(_enemiesInViewport[i].GetComponent<EnemyMove>())) _enemiesVisible.Add(_enemiesInViewport[i].GetComponent<EnemyMove>());
                                Debug.DrawRay(origin, direction);
                            }
                        }
                    }

                    for (int i = 0; i < _enemiesInViewport.Count; i++)
                    {
                        if (tempHit[i].collider != null)
                        {
                            if (tempHit[i].collider.tag == "Level")
                            {
                                if (_enemiesVisible.Contains(_enemiesInViewport[i].GetComponent<EnemyMove>()))
                                {
                                    _enemiesVisible.Remove(_enemiesInViewport[i].GetComponent<EnemyMove>());
                                    _selectedEnemy = _selectedEnemy <= 0 ? 0 : _selectedEnemy - 1;
                                    _previousEnemy = _previousEnemy <= 0 ? 0 : _previousEnemy - 1;
                                    if (_enemiesVisible.Count <= 0)
                                    {
                                        _VATSactive = false;
                                    }
                                }
                            }
                        }
                    }

                    if (_enemiesVisible[_selectedEnemy].indicatorSelectors == null)
                    {
                        _enemiesVisible[_selectedEnemy].indicatorSelectors = new Stack<RectTransform>[_enemiesVisible[_selectedEnemy].limbs.Length];
                        for (int i = 0; i < _enemiesVisible[_selectedEnemy].indicatorSelectors.Length; i++)
                        {
                            _enemiesVisible[_selectedEnemy].indicatorSelectors[i] = new Stack<RectTransform>();
                        }
                    }

                    Vector3 lerpedPosition = Vector3.Lerp(_enemiesVisible[_previousEnemy].transform.position + (Vector3.up * (_enemiesVisible[_previousEnemy].GetComponent<CharacterController>().height / 2)),
                                            _enemiesVisible[_selectedEnemy].transform.position + (Vector3.up * (_enemiesVisible[_selectedEnemy].GetComponent<CharacterController>().height / 2)),
                                            _moveLerpElapsed);

                    VATSCamera.depth = 99;
                    _shootClass.mouseRestriction = true;
                    _moveClass.mouseRestriction = true;
                    _moveClass.stationary = true;

                    Time.timeScale = 0.01f;

                    VATSEnemyHealthName.text = _enemiesVisible[_selectedEnemy].GetComponent<CharacterStats>().name.ToUpper();
                    VATSEnemyHealthBar.maxValue = _enemiesVisible[_selectedEnemy].GetComponent<CharacterStats>().maxHealthPoints;
                    VATSEnemyHealthBar.value = _enemiesVisible[_selectedEnemy].GetComponent<CharacterStats>().healthPoints;

                    VATSHitChanceIndicators.position = VATSCamera.WorldToScreenPoint(_enemiesVisible[_selectedEnemy].transform.position + (Vector3.up * (_enemiesVisible[_selectedEnemy].GetComponent<CharacterController>().height / 2)));
                    for (int ii = 0; ii < _enemiesVisible[_selectedEnemy].limbs.Length; ii++)
                    {
                        if (!_indicators.Exists(x => x.limb.bones == _enemiesVisible[_selectedEnemy].limbs[ii].bones))
                        {
                            _indicators.Add(new IndicatorLimbPairing() { indicator = Instantiate(VATSIndicator, VATSCamera.WorldToScreenPoint(_enemiesVisible[_selectedEnemy].limbs[ii].indicatorPosition.position), Quaternion.identity, VATSHitChanceIndicators).GetComponent<Indicator>(), limb = _enemiesVisible[_selectedEnemy].limbs[ii] });
                            if(_enemiesVisible[_selectedEnemy].indicatorSelectors[ii].Any())
                            {
                                for(int iii = 0; iii < _enemiesVisible[_selectedEnemy].indicatorSelectors[ii].Count; iii++)
                                {
                                    RectTransform newSelector = Instantiate(VATSIndicatorSelector, _indicators[ii].indicator.transform);
                                    newSelector.offsetMin = new Vector2(-5 - (10 * iii), newSelector.offsetMin.y);
                                    newSelector.offsetMax = new Vector2(5 + (10 * iii), newSelector.offsetMax.y);
                                }
                            }
                        }
                    }

                    for(int ii = 0; ii < _indicators.Count; ii++)
                    {
                        _indicators[ii].indicator.transform.position = VATSCamera.WorldToScreenPoint(_indicators[ii].limb.indicatorPosition.position);
                        float hitChance = 100f;
                        foreach (Collider bone in _indicators[ii].limb.bones)
                        {
                            Ray tempRayTop = new Ray(VATSCamera.transform.position, (bone.bounds.center + (bone.transform.up * bone.bounds.extents.y)) - VATSCamera.transform.position);
                            Ray tempRayCentre = new Ray(VATSCamera.transform.position, bone.bounds.center - VATSCamera.transform.position);
                            Ray tempRayBottom = new Ray(VATSCamera.transform.position, (bone.bounds.center - (bone.transform.up * bone.bounds.extents.y)) - VATSCamera.transform.position);

                            RaycastHit tempRayHitTop, tempRayHitCentre, tempRayHitBottom;

                            if (Physics.Raycast(tempRayTop, out tempRayHitTop, Mathf.Min(VATSRange, Vector3.Distance((bone.bounds.center + (bone.transform.up * bone.bounds.extents.y)), VATSCamera.transform.position)), ~LayerMask.GetMask("PlayerModel", "PlayerController", "EnemyController")))
                            {
                                if (_indicators[ii].limb.bones.Contains(tempRayHitTop.collider))
                                {
                                    hitChance -= Mathf.Clamp(tempRayHitTop.distance * VATSHitChanceModifier / _indicators[ii].limb.bones.Length, 0f, ((1f / 3f) / _indicators[ii].limb.bones.Length) * 100f);
                                }
                                else
                                {
                                    hitChance -= 100f * ((1f / 3f) / _indicators[ii].limb.bones.Length);
                                }
                            }

                            if (Physics.Raycast(tempRayCentre, out tempRayHitCentre, Mathf.Min(VATSRange, Vector3.Distance(bone.bounds.center, VATSCamera.transform.position)), ~LayerMask.GetMask("PlayerModel", "PlayerController", "EnemyController")))
                            {
                                if (_indicators[ii].limb.bones.Contains(tempRayHitCentre.collider))
                                {
                                    hitChance -= Mathf.Clamp(tempRayHitCentre.distance * VATSHitChanceModifier / _indicators[ii].limb.bones.Length, 0f, ((1f / 3f) / _indicators[ii].limb.bones.Length) * 100f);
                                }
                                else
                                {
                                    hitChance -= 100f * ((1f / 3f) / _indicators[ii].limb.bones.Length);
                                }
                            }

                            if (Physics.Raycast(tempRayBottom, out tempRayHitBottom, Mathf.Min(VATSRange, Vector3.Distance((bone.bounds.center - (bone.transform.up * bone.bounds.extents.y)), VATSCamera.transform.position)), ~LayerMask.GetMask("PlayerModel", "PlayerController", "EnemyController")))
                            {
                                if (_indicators[ii].limb.bones.Contains(tempRayHitBottom.collider))
                                {
                                    hitChance -= Mathf.Clamp(tempRayHitBottom.distance * VATSHitChanceModifier / _indicators[ii].limb.bones.Length, 0f, ((1f / 3f) / _indicators[ii].limb.bones.Length) * 100f);
                                }
                                else
                                {
                                    hitChance -= 100f * ((1f / 3f) / _indicators[ii].limb.bones.Length);
                                }
                            }
                        }
                        hitChance = Mathf.Clamp(hitChance, 0f, 100f);
                        _indicators[ii].indicator.hitChanceText.text = hitChance.ToString("0");
                        _indicators[ii].limb.hitChance = hitChance;
                    }

                    SetLayer(_enemiesVisible[_selectedEnemy].transform.GetChild(0).gameObject, LayerMask.NameToLayer("SelectedEnemy"), true);

                    if (_mouseHoverMode)
                    {
                        RaycastHit tempHit2;
                        Ray tempRay = VATSCamera.ScreenPointToRay(Input.mousePosition);

                        if (Physics.Raycast(tempRay, out tempHit2, VATSRange, LayerMask.GetMask("SelectedEnemy")))
                        {
                            if (tempHit2.transform.tag == "Enemy")
                            {
                                Limbs tempLimb = _enemiesVisible[_selectedEnemy].limbs.First(x => x.bones.Contains(tempHit2.collider));
                                _currentLimb = _enemiesVisible[_selectedEnemy].limbs.ToList().FindIndex(x => x.bones == tempLimb.bones);

                                tempLimb.joints.layer = LayerMask.NameToLayer("VATSEnemy");
                                tempLimb.muscles.layer = LayerMask.NameToLayer("VATSEnemy");

                                if ((Input.GetMouseButtonDown(0) || Input.GetButtonDown("Enter VATS")) && !_justEnteredVATS && _currentVATSCost + VATSCost <= _playerStats.actionPoints)
                                {
                                    GameObject tempAPIndicator = Instantiate(APIndicator, APIndicatorParent);
                                    tempAPIndicator.GetComponent<RectTransform>().sizeDelta = new Vector2(APIndicatorParent.rect.width * (VATSCost / _playerStats.maxActionPoints) - APIndicatorParent.GetComponent<HorizontalLayoutGroup>().spacing, 0f);

                                    _currentVATSCost += VATSCost;
                                    _VATSSetupOrder.Push(tempLimb);

                                    RectTransform selector = Instantiate(VATSIndicatorSelector, _indicators.First(x => x.limb.bones == tempLimb.bones).indicator.transform);
                                    selector.offsetMin = new Vector2(-5 - (10 * _enemiesVisible[_selectedEnemy].indicatorSelectors[_currentLimb].Count), selector.offsetMin.y);
                                    selector.offsetMax = new Vector2(5 + (10 * _enemiesVisible[_selectedEnemy].indicatorSelectors[_currentLimb].Count), selector.offsetMax.y);
                                    _enemiesVisible[_selectedEnemy].indicatorSelectors[_currentLimb].Push(selector);
                                }
                                else if ((Input.GetMouseButtonDown(0) || Input.GetButtonDown("Enter VATS")) && _currentVATSCost + VATSCost > _playerStats.actionPoints)
                                {
                                    _flashTimeStepMax = 0f;
                                }

                                if ((Input.GetMouseButtonDown(1) || Input.GetButtonDown("Exit VATS")) && _VATSSetupOrder.Any())
                                {
                                    Destroy(APIndicatorParent.GetChild(APIndicatorParent.childCount - 1).gameObject);

                                    _currentVATSCost -= VATSCost;
                                    _VATSSetupOrder.Pop();
                                    RectTransform selector = _enemiesVisible[_selectedEnemy].indicatorSelectors[_currentLimb].Pop();
                                    Destroy(selector.gameObject);
                                }
                            }
                        }
                    }
                    else if (!Input.GetButtonDown("Vertical"))
                    {
                        Limbs tempLimb = _enemiesVisible[_selectedEnemy].limbs[_currentLimb];

                        tempLimb.joints.layer = LayerMask.NameToLayer("VATSEnemy");
                        tempLimb.muscles.layer = LayerMask.NameToLayer("VATSEnemy");

                        if ((Input.GetMouseButtonDown(0) || Input.GetButtonDown("Enter VATS")) && !_justEnteredVATS && _currentVATSCost + VATSCost <= _playerStats.actionPoints)
                        {
                            GameObject tempAPIndicator = Instantiate(APIndicator, APIndicatorParent);
                            tempAPIndicator.GetComponent<RectTransform>().sizeDelta = new Vector2(APIndicatorParent.rect.width * (VATSCost / _playerStats.maxActionPoints) - APIndicatorParent.GetComponent<HorizontalLayoutGroup>().spacing, 0f);

                            _currentVATSCost += VATSCost;
                            _VATSSetupOrder.Push(tempLimb);

                            RectTransform selector = Instantiate(VATSIndicatorSelector, _indicators.First(x => x.limb.bones == tempLimb.bones).indicator.transform);
                            selector.offsetMin = new Vector2(-5 - (10 * _enemiesVisible[_selectedEnemy].indicatorSelectors[_currentLimb].Count), selector.offsetMin.y);
                            selector.offsetMax = new Vector2(5 + (10 * _enemiesVisible[_selectedEnemy].indicatorSelectors[_currentLimb].Count), selector.offsetMax.y);
                            _enemiesVisible[_selectedEnemy].indicatorSelectors[_currentLimb].Push(selector);
                        }
                        else if ((Input.GetMouseButtonDown(0) || Input.GetButtonDown("Enter VATS")) && _currentVATSCost + VATSCost > _playerStats.actionPoints)
                        {
                            _flashTimeStepMax = 0f;
                        }

                        if ((Input.GetMouseButtonDown(1) || Input.GetButtonDown("Exit VATS")) && _VATSSetupOrder.Any())
                        {
                            Destroy(APIndicatorParent.GetChild(APIndicatorParent.childCount - 1).gameObject);

                            _currentVATSCost -= VATSCost;
                            _VATSSetupOrder.Pop();
                            RectTransform selector = _enemiesVisible[_selectedEnemy].indicatorSelectors[_currentLimb].Pop();
                            Destroy(selector.gameObject);
                        }
                    }

                    if (Input.GetButtonDown("Confirm VATS"))
                    {
                        if (_VATSSetupOrder.Any())
                        {
                            _VATSShootingOrder = new Queue<Limbs>(_VATSSetupOrder.Reverse());
                            _VATSSetupOrder.Clear();
                            _inAnimation = true;
                        }
                        else
                        {
                            _VATSactive = false;
                        }
                    }

                    if (Input.GetButtonDown("Vertical")) _mouseHoverMode = false;
                    else if (Mathf.Abs(Input.GetAxisRaw("Mouse X") + Input.GetAxisRaw("Mouse Y")) > 0) _mouseHoverMode = true;

                    if (_moveLerpElapsed < 1 && _previousEnemy != _selectedEnemy) _moveLerpElapsed += Time.unscaledDeltaTime * lerpModifier;
                    VATSCamera.transform.LookAt(lerpedPosition);

                    _newFOV = Mathf.Lerp(_previousFOV,
                                 2 * Mathf.Rad2Deg * Mathf.Acos(Mathf.Min(Vector3.Distance(VATSCamera.transform.position, _enemiesVisible[_selectedEnemy].transform.position + (Vector3.up * (_enemiesVisible[_selectedEnemy].GetComponent<CharacterController>().height / 2))),
                                                                          Vector3.Distance(VATSCamera.transform.position, _enemiesVisible[_selectedEnemy].transform.position)) /
                                                                Mathf.Max(Vector3.Distance(VATSCamera.transform.position, _enemiesVisible[_selectedEnemy].transform.position + (Vector3.up * (_enemiesVisible[_selectedEnemy].GetComponent<CharacterController>().height / 2))),
                                                                          Vector3.Distance(VATSCamera.transform.position, _enemiesVisible[_selectedEnemy].transform.position))),
                                        _zoomChangeLerpElapsed);

                    if (_zoomChangeLerpElapsed < 1) _zoomChangeLerpElapsed += Time.unscaledDeltaTime * lerpModifier;
                    if (_zoomLerpElapsed < 1) _zoomLerpElapsed += Time.unscaledDeltaTime * lerpModifier;

                    if (_flashTimeStepMax < 2)
                    {
                        _flashTimeStepMax += ((2 / APFlashLength) * Time.unscaledDeltaTime);
                        if (_flashFlip)
                        {
                            _flashTimeStep += Mathf.Min((2 / APFlashLength) * Time.unscaledDeltaTime, 1f - _flashTimeStep);
                            if (_flashTimeStep >= 1f) _flashFlip = false;
                        }
                        else
                        {
                            _flashTimeStep -= ((2 / APFlashLength) * Time.unscaledDeltaTime);
                            if (_flashTimeStep <= 0f) _flashFlip = true;
                        }
                    }
                    else
                    {
                        _flashFlip = true;
                        _flashTimeStep = 0;
                    }
                    playerActionBar.fillRect.GetComponent<Image>().color = Color.Lerp(_sliderBaseColor, new Color(_sliderBaseColor.r, _sliderBaseColor.g, _sliderBaseColor.b, 0.25f), _flashTimeStep);
                    _justEnteredVATS = false;
                }
                else
                {
                    Cursor.visible = false;

                    for (int i = 0; i < _indicators.Count; i++)
                    {
                        Destroy(_indicators[i].indicator.gameObject);
                    }

                    _indicators.Clear();

                    for (int i = APIndicatorParent.childCount - 1; i >= 0; i--)
                    {
                        Destroy(APIndicatorParent.GetChild(i).gameObject);
                    }

                    if (!_animationPlaying)
                    {
                        VATSCinemaCameraBrain.gameObject.SetActive(true);
                        StartCoroutine(PlayShot());
                        _animationPlaying = true;
                    }
                }
                break;
            case false:
                RaycastHit hit;
                if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit))
                {
                    VATSEnemyHealthName.transform.parent.gameObject.SetActive(hit.transform.root.GetComponent<EnemyMove>() && !hit.transform.root.GetComponent<EnemyMove>().isDead);

                    if (hit.transform.root.GetComponent<EnemyMove>() && !hit.transform.root.GetComponent<EnemyMove>().isDead)
                    {
                        VATSEnemyHealthName.text = hit.transform.root.GetComponent<CharacterStats>().name.ToUpper();
                        VATSEnemyHealthBar.maxValue = hit.transform.root.GetComponent<CharacterStats>().maxHealthPoints;
                        VATSEnemyHealthBar.value = hit.transform.root.GetComponent<CharacterStats>().healthPoints;
                    }
                }

                for (int i = 0; i < _indicators.Count; i++)
                {
                    Destroy(_indicators[i].indicator.gameObject);
                }

                _indicators.Clear();
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                _selectedEnemy = 0;
                _previousEnemy = _selectedEnemy;
                _currentVATSCost = 0;
                VATSCinemaCameraBrain.depth = -99;
                VATSCamera.depth = -99;
                Camera.main.depth = 99;
                VATSCamera.transform.rotation = Camera.main.transform.rotation;
                _zoomLerpElapsed = 0;
                for (int i = 0; i < _enemiesVisible.Count; i++)
                {
                    SetLayer(_enemiesVisible[i].transform.GetChild(0).gameObject, LayerMask.NameToLayer("Default"), true);
                }
                _enemiesVisible.Clear();
                _shootClass.mouseRestriction = false;
                _moveClass.mouseRestriction = false;
                _moveClass.stationary = false;
                Time.timeScale = 1f;
                VATSCinemaCameraBrain.gameObject.SetActive(false);
                break;
        }
        Time.fixedDeltaTime = _fixedTimeStepBase * Time.timeScale;

        VATSHeadsUpDisplay.SetActive(_VATSactive);
        VATSCamera.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, _newFOV, _zoomLerpElapsed);

        if (_moveClass.mouseRestriction)
        {
            if (_enemiesVisible.Count > 1)
            {
                if (Input.GetButtonDown("Horizontal") && Input.GetAxisRaw("Horizontal") > 0)
                {
                    for (int i = 0; i < _indicators.Count; i++)
                    {
                        Destroy(_indicators[i].indicator.gameObject);
                    }

                    _indicators.Clear();
                    _previousEnemy = _selectedEnemy;
                    _previousFOV = _newFOV;
                    _zoomChangeLerpElapsed = 0;
                    _moveLerpElapsed = 0;
                    _selectedEnemy = _selectedEnemy >= _enemiesVisible.Count - 1 ? 0 : _selectedEnemy + 1;

                    SetLayer(_enemiesVisible[_selectedEnemy].transform.GetChild(0).gameObject, LayerMask.NameToLayer("SelectedEnemy"), true);
                    SetLayer(_enemiesVisible[_previousEnemy].transform.GetChild(0).gameObject, LayerMask.NameToLayer("Default"), true);
                }
                else if (Input.GetButtonDown("Horizontal") && Input.GetAxisRaw("Horizontal") < 0)
                {
                    for (int i = 0; i < _indicators.Count; i++)
                    {
                        Destroy(_indicators[i].indicator.gameObject);
                    }

                    _indicators.Clear();
                    _previousEnemy = _selectedEnemy;
                    _previousFOV = _newFOV;
                    _zoomChangeLerpElapsed = 0;
                    _moveLerpElapsed = 0;
                    _selectedEnemy = _selectedEnemy <= 0 ? _enemiesVisible.Count - 1 : _selectedEnemy - 1;

                    SetLayer(_enemiesVisible[_selectedEnemy].transform.GetChild(0).gameObject, LayerMask.NameToLayer("SelectedEnemy"), true);
                    SetLayer(_enemiesVisible[_previousEnemy].transform.GetChild(0).gameObject, LayerMask.NameToLayer("Default"), true);
                }
            }

            if (Input.GetButtonDown("Vertical") && Input.GetAxisRaw("Vertical") > 0)
            {
                _currentLimb = _currentLimb >= _enemiesVisible[_selectedEnemy].limbs.Length - 1 ? 0 : _currentLimb + 1;
                _mouseHoverMode = false;
            }
            else if (Input.GetButtonDown("Vertical") && Input.GetAxisRaw("Vertical") < 0)
            {
                _currentLimb = _currentLimb <= 0 ? _enemiesVisible[_selectedEnemy].limbs.Length - 1 : _currentLimb - 1;
                _mouseHoverMode = false;
            }
        }
    }

    IEnumerator PlayShot()
    {
        Time.timeScale = 0.001f;
        VATSCinemaCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = 1f / Time.timeScale;
        while (_VATSShootingOrder.Any())
        {
            _playerStats.actionPoints -= _currentVATSCost / _VATSShootingOrder.Count;
            _currentVATSCost -= _currentVATSCost / _VATSShootingOrder.Count;
            Limbs limb = _VATSShootingOrder.Dequeue();

            VATSEnemyHealthName.text = limb.bones[0].transform.root.GetComponent<CharacterStats>().name.ToUpper();
            VATSEnemyHealthBar.maxValue = limb.bones[0].transform.root.GetComponent<CharacterStats>().maxHealthPoints;
            VATSEnemyHealthBar.value = limb.bones[0].transform.root.GetComponent<CharacterStats>().healthPoints;
            VATSCamera.depth = -99;
            VATSCinemaCameraBrain.depth = 99;
            Camera.main.transform.LookAt(limb.indicatorPosition.transform);
            yield return null;
            bool missed = (Random.Range(0f, 100f) > limb.hitChance);

            Bullet bullet = _shootClass.Fire(limb.indicatorPosition.transform, missed).GetComponent<Bullet>();
            if (bullet != null)
            {
                VATSCinemaCamera.LookAt = bullet.transform;

                bullet.missed = false;
                bullet.startPos = bullet.transform.position;
                bullet.targetPos = limb.indicatorPosition.transform.position;
                while (bullet != null && !bullet.missed)
                {
                    if (Vector3.Distance(bullet.transform.position, bullet.startPos) > Vector3.Distance(bullet.targetPos, bullet.startPos)) bullet.missed = true;
                    if (!missed && Physics.OverlapCapsule(bullet.GetComponent<CapsuleCollider>().bounds.center - new Vector3(0, bullet.GetComponent<CapsuleCollider>().bounds.extents.y, 0),
                                                          bullet.GetComponent<CapsuleCollider>().bounds.center + new Vector3(0, bullet.GetComponent<CapsuleCollider>().bounds.extents.y, 0),
                                                           bullet.GetComponent<CapsuleCollider>().radius).Any(x => limb.bones.Contains(x) || x == limb.bones[0].transform.root.GetComponent<CharacterController>()))
                    {
                        CharacterStats hitCharacter = limb.bones[0].transform.root.GetComponent<CharacterStats>();
                        if (hitCharacter != null)
                        {
                            hitCharacter.TakeDamage(10f);
                        }

                        Destroy(bullet.gameObject);
                    }
                    yield return null;
                }
                if (!missed) VATSCinemaCamera.LookAt = limb.indicatorPosition;
                if (bullet != null) Destroy(bullet.gameObject);
            }

            VATSEnemyHealthName.text = limb.bones[0].transform.root.GetComponent<CharacterStats>().name.ToUpper();
            VATSEnemyHealthBar.maxValue = limb.bones[0].transform.root.GetComponent<CharacterStats>().maxHealthPoints;
            VATSEnemyHealthBar.value = limb.bones[0].transform.root.GetComponent<CharacterStats>().healthPoints;
            yield return new WaitForSecondsRealtime(VATSHangTime);
            VATSCinemaCamera.LookAt = _shootClass.weaponProjectilePos;
        }

        VATSCinemaCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = 0;

        foreach (EnemyMove enemy in _enemiesVisible)
            if (enemy.indicatorSelectors != null)
                foreach (Stack<RectTransform> stack in enemy.indicatorSelectors)
                    stack.Clear();
        _inAnimation = false;
        _VATSactive = false;
        _animationPlaying = false;
    }

    public static void SetLayer(GameObject gameObject, int layer, bool includeChildren = false)
    {
        if (!gameObject) return;
        if (!includeChildren)
        {
            gameObject.layer = layer;
            return;
        }

        foreach (var child in gameObject.GetComponentsInChildren(typeof(Transform), true))
        {
            child.gameObject.layer = layer;
        }
    }
}
