#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_6000_0_OR_NEWER|| TUANJIE_1_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif

namespace TapTapMiniGame.Editor
{
    /// <summary>
    /// TapTap 小游戏配置帮助类
    /// 用于统一管理构建配置的加载和游戏信息的读取
    /// </summary>
    public static class TapMiniGameConfigHelper
    {
        /// <summary>
        /// 加载所有可用的 TapTap 构建配置
        /// </summary>
        /// <returns>构建配置列表</returns>
        public static List<BuildProfileInfo> LoadAllBuildProfiles()
        {
            var buildProfiles = new List<BuildProfileInfo>();
            
            try
            {
#if UNITY_6000_0_OR_NEWER|| TUANJIE_1_0_OR_NEWER
                LoadBuildProfiles(buildProfiles);
#endif
                
                // 始终尝试加载传统配置作为备用
                LoadLegacyConfig(buildProfiles);
                
                if (buildProfiles.Count == 0)
                {
                    Debug.LogError("No TapTap build profiles found. Please build your game first!");
                }
                else
                {
                    Debug.Log($"Loaded {buildProfiles.Count} TapTap build profile(s)");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading build profiles: {e.Message}");
            }
            
            return buildProfiles;
        }
        
        /// <summary>
        /// 从指定的 DST 路径加载游戏信息
        /// </summary>
        /// <param name="dstPath">构建输出目录路径</param>
        /// <returns>游戏信息，如果加载失败返回 null</returns>
        public static GameInfo LoadGameInfoFromDst(string dstPath)
        {
            if (string.IsNullOrEmpty(dstPath))
            {
                Debug.LogError("DST path is empty. Build configuration is invalid!");
                return null;
            }

            string gameJsonPath = Path.Combine(dstPath, "minigame", "game.json");

            if (!File.Exists(gameJsonPath))
            {
                // 改为Log级别，因为有保底机制，这不是致命错误
                Debug.Log($"game.json not found at: {gameJsonPath}. Will try to load from config as fallback.");
                return null;
            }

            try
            {
                string jsonContent = File.ReadAllText(gameJsonPath);
                GameInfo gameInfo = JsonUtility.FromJson<GameInfo>(jsonContent);
                
                if (gameInfo == null || string.IsNullOrEmpty(gameInfo.appId))
                {
                    Debug.LogError($"Invalid game.json at: {gameJsonPath}. Missing appId field!");
                    return null;
                }
                
                Debug.Log($"Game info loaded: appId={gameInfo.appId}, productName={gameInfo.productName}");
                return gameInfo;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing game.json at {gameJsonPath}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保底机制：当 game.json 不存在时，从构建配置中读取游戏信息
        /// 优先级：1. Build Profile (Unity 2022+/团结引擎) → 2. Legacy Config
        /// </summary>
        /// <param name="buildProfile">构建配置信息</param>
        /// <returns>从配置文件读取的游戏信息，失败返回 null</returns>
        public static GameInfo LoadGameInfoFallback(BuildProfileInfo buildProfile)
        {
            if (buildProfile == null)
            {
                Debug.Log("[Fallback] BuildProfileInfo is null, cannot load fallback game info");
                return null;
            }

            string appId = null;

            // 优先级1：尝试从 Build Profile 读取 appId (Unity 2022+/团结引擎)
#if UNITY_6000_0_OR_NEWER|| TUANJIE_1_0_OR_NEWER
            appId = LoadAppIdFromBuildProfile(buildProfile.profilePath);
            if (!string.IsNullOrEmpty(appId))
            {
                Debug.Log($"[Fallback] Loaded appId from Build Profile: {appId}");
            }
#endif

            // 优先级2：如果 Build Profile 没有读取到，尝试从 Legacy Config 读取
            if (string.IsNullOrEmpty(appId))
            {
                appId = LoadAppIdFromLegacyConfig();
                if (!string.IsNullOrEmpty(appId))
                {
                    Debug.Log($"[Fallback] Loaded appId from Legacy Config: {appId}");
                }
            }

            // 如果成功读取到 appId，构造 GameInfo
            if (!string.IsNullOrEmpty(appId))
            {
                GameInfo gameInfo = new GameInfo
                {
                    appId = appId,
                    productName = Application.productName,
                    companyName = Application.companyName,
                    productVersion = Application.version,
                    convertScriptVersion = "",
                    convertToolVersion = ""
                };

                Debug.Log($"[Fallback] GameInfo created from config: appId={gameInfo.appId}, productName={gameInfo.productName}");
                return gameInfo;
            }

            // 没有找到appId，但这不是错误，可能是新项目还没配置
            Debug.Log("[TapMiniGame] 未找到小游戏 appId 配置。如需使用调试工具，请先配置：");
            Debug.Log("[TapMiniGame] 1. 在 Build Profile 或 MiniGameConfig.asset 中设置 appId");
            Debug.Log("[TapMiniGame] 2. 或者先构建一次小游戏以生成 game.json 文件");
            return null;
        }

#if UNITY_6000_0_OR_NEWER|| TUANJIE_1_0_OR_NEWER
        /// <summary>
        /// 从 Build Profile 中读取 appId (Unity 2022+/团结引擎)
        /// </summary>
        private static string LoadAppIdFromBuildProfile(string profilePath)
        {
            try
            {
                if (string.IsNullOrEmpty(profilePath))
                {
                    Debug.Log("[Fallback] Build profile path is empty");
                    return null;
                }

                // 加载 Build Profile
                var buildProfile = AssetDatabase.LoadAssetAtPath<ScriptableObject>(profilePath);
                if (buildProfile == null)
                {
                    Debug.Log($"[Fallback] Failed to load build profile at: {profilePath}");
                    return null;
                }

                // 检查是否是 BuildProfile 类型
                if (!(buildProfile is BuildProfile profile))
                {
                    Debug.Log($"[Fallback] Profile is not BuildProfile type: {buildProfile.GetType().Name}");
                    return null;
                }

                // 获取 miniGameSettings
                var miniGameSettings = profile.miniGameSettings;
                if (miniGameSettings == null)
                {
                    Debug.Log("[Fallback] miniGameSettings is null");
                    return null;
                }

                // 通过反射获取 ProjectConf
                var bindingFlags = System.Reflection.BindingFlags.Instance |
                                  System.Reflection.BindingFlags.NonPublic |
                                  System.Reflection.BindingFlags.Public;

                var projectConfField = miniGameSettings.GetType().GetField("ProjectConf", bindingFlags);
                if (projectConfField == null)
                {
                    Debug.Log("[Fallback] ProjectConf field not found in miniGameSettings");
                    return null;
                }

                var projectConf = projectConfField.GetValue(miniGameSettings);
                if (projectConf == null)
                {
                    Debug.Log("[Fallback] ProjectConf value is null");
                    return null;
                }

                // 通过反射获取 Appid
                var appIdField = projectConf.GetType().GetField("Appid", bindingFlags);
                if (appIdField == null)
                {
                    Debug.Log("[Fallback] Appid field not found in ProjectConf");
                    return null;
                }

                string appId = (string)appIdField.GetValue(projectConf);
                if (!string.IsNullOrEmpty(appId))
                {
                    Debug.Log($"[Fallback] Successfully extracted appId from Build Profile: {appId}");
                    return appId;
                }

                Debug.Log("[Fallback] Appid field is empty in ProjectConf");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Fallback] Error loading appId from Build Profile: {e.Message}");
                return null;
            }
        }
#endif

        /// <summary>
        /// 从传统的 MiniGameConfig.asset 中读取 appId
        /// </summary>
        private static string LoadAppIdFromLegacyConfig()
        {
            try
            {
                // 加载传统 MiniGameConfig.asset
                var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TapTapMiniGame/Editor/MiniGameConfig.asset");
                if (config == null)
                {
                    Debug.Log("[Fallback] Legacy MiniGameConfig.asset not found");
                    return null;
                }

                // 通过反射获取 ProjectConf
                var bindingFlags = System.Reflection.BindingFlags.Instance |
                                  System.Reflection.BindingFlags.NonPublic |
                                  System.Reflection.BindingFlags.Public;

                var projectConfField = config.GetType().GetField("ProjectConf", bindingFlags);
                if (projectConfField == null)
                {
                    Debug.Log("[Fallback] ProjectConf field not found in legacy config");
                    return null;
                }

                var projectConf = projectConfField.GetValue(config);
                if (projectConf == null)
                {
                    Debug.Log("[Fallback] ProjectConf value is null in legacy config");
                    return null;
                }

                // 通过反射获取 Appid
                var appIdField = projectConf.GetType().GetField("Appid", bindingFlags);
                if (appIdField == null)
                {
                    Debug.Log("[Fallback] Appid field not found in ProjectConf");
                    return null;
                }

                string appId = (string)appIdField.GetValue(projectConf);
                if (!string.IsNullOrEmpty(appId))
                {
                    Debug.Log($"[Fallback] Successfully extracted appId from Legacy Config: {appId}");
                    return appId;
                }

                Debug.Log("[Fallback] Appid field is empty in legacy config");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Fallback] Error loading appId from legacy config: {e.Message}");
                return null;
            }
        }

#if UNITY_6000_0_OR_NEWER|| TUANJIE_1_0_OR_NEWER
        private static void LoadBuildProfiles(List<BuildProfileInfo> buildProfiles)
        {
            string buildProfilesPath = "Assets/Settings/Build Profiles";
            
            if (!Directory.Exists(buildProfilesPath))
            {
                Debug.Log("Build Profiles directory not found");
                return;
            }
            
            // 获取所有.asset文件
            string[] assetFiles = Directory.GetFiles(buildProfilesPath, "*.asset", SearchOption.TopDirectoryOnly);
            
            foreach (string assetFile in assetFiles)
            {
                try
                {
                    // 转换为相对路径用于AssetDatabase
                    string relativePath = assetFile.Replace(Application.dataPath, "Assets").Replace("\\", "/");
                    
                    // 使用AssetDatabase加载ScriptableObject
                    var buildProfile = AssetDatabase.LoadAssetAtPath<ScriptableObject>(relativePath);
                    
                    if (buildProfile != null && IsTapTapProfile(buildProfile))
                    {
                        string dst = ExtractDSTFromBuildProfile(buildProfile);
                        if (!string.IsNullOrEmpty(dst))
                        {
                            string profileName = Path.GetFileNameWithoutExtension(assetFile);
                            buildProfiles.Add(new BuildProfileInfo(profileName, relativePath, dst, "TapTap"));
                            Debug.Log($"Found TapTap Build Profile: {profileName} -> {dst}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing build profile {assetFile}: {e.Message}");
                }
            }
        }
        
        /// <summary>
        /// 判断是否为 TapTap 配置文件
        /// </summary>
        private static bool IsTapTapProfile(ScriptableObject profile)
        {
            try
            {
                string profileTypeName = profile.GetType().Name;
                string profileTypeFullName = profile.GetType().FullName;
                
                // 对于DouYin配置文件，直接排除
                if (profileTypeName.Contains("DouYin", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"发现DouYin配置文件，跳过: {profileTypeName}");
                    return false;
                }
                
                // 检查是否是BuildProfile或其子类，并且有miniGameSettings属性
                if (profile is BuildProfile buildProfile)
                {
                    Debug.Log($"确认是BuildProfile类型: {profileTypeName}");
                    
                    // 使用公共属性获取miniGameSettings
                    var miniGameSettings = buildProfile.miniGameSettings;
                    if (miniGameSettings == null)
                    {
                        Debug.Log($"miniGameSettings 属性值为空");
                        return false;
                    }
                    
                    // 检查MiniGameSettings的类型和命名空间
                    var settingsType = miniGameSettings.GetType();
                    string typeName = settingsType.Name;
                    string nameSpace = settingsType.Namespace;
                    
                    Debug.Log($"MiniGameSettings类型: {typeName}, 命名空间: {nameSpace}");
                    
                    // TapTap配置文件的独有标识：
                    // 1. 类型名为 TapTapMiniGameSettings
                    // 2. 命名空间为 TapTapMiniGame
                    bool isTapTapType = typeName.Equals("TapTapMiniGameSettings", StringComparison.OrdinalIgnoreCase);
                    bool isTapTapNamespace = nameSpace != null && nameSpace.Equals("TapTapMiniGame", StringComparison.OrdinalIgnoreCase);
                    
                    if (isTapTapType && isTapTapNamespace)
                    {
                        Debug.Log("通过类型和命名空间确认为TapTap配置文件");
                        return true;
                    }
                    
                    // 备用检查：检查HostName属性
                    string hostName = buildProfile.GetHostName();
                    Debug.Log($"检查到HostName: {hostName}");
                    if (!string.IsNullOrEmpty(hostName) && hostName.Equals("TapTap", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log("通过HostName确认为TapTap配置文件");
                        return true;
                    }
                    
                    Debug.Log($"不是TapTap配置文件: MiniGameSettings类型={typeName}, 命名空间={nameSpace}, HostName={hostName}");
                    return false;
                }
                
                Debug.Log($"不是BuildProfile类型，跳过: {profileTypeFullName}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error checking TapTap profile: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 从 BuildProfile 中提取 DST 路径
        /// </summary>
        private static string ExtractDSTFromBuildProfile(ScriptableObject profile)
        {
            try
            {
                // 只处理BuildProfile类型
                if (!(profile is BuildProfile buildProfile))
                {
                    Debug.Log($"配置文件类型 {profile.GetType().Name} 不是BuildProfile，跳过DST提取");
                    return null;
                }

                // 使用公共属性获取miniGameSettings
                var miniGameSettings = buildProfile.miniGameSettings;
                if (miniGameSettings == null)
                {
                    Debug.Log("miniGameSettings属性值为空");
                    return null;
                }
                
                // 通过反射获取DST路径（ProjectConf可能仍然是私有字段）
                var bindingFlags = System.Reflection.BindingFlags.Instance | 
                                  System.Reflection.BindingFlags.NonPublic | 
                                  System.Reflection.BindingFlags.Public;
                
                var projectConfField = miniGameSettings.GetType().GetField("ProjectConf", bindingFlags);
                if (projectConfField == null)
                {
                    Debug.Log("MiniGameSettings中未找到ProjectConf字段");
                    return null;
                }
                
                var projectConf = projectConfField.GetValue(miniGameSettings);
                if (projectConf == null)
                {
                    Debug.Log("ProjectConf字段值为空");
                    return null;
                }
                
                var dstField = projectConf.GetType().GetField("DST", bindingFlags);
                if (dstField == null)
                {
                    Debug.Log("ProjectConf中未找到DST字段");
                    return null;
                }
                
                string dst = (string)dstField.GetValue(projectConf);
                Debug.Log($"成功提取DST路径: {dst}");
                return dst;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error extracting DST from build profile: {e.Message}");
                return null;
            }
        }
#endif
        
        /// <summary>
        /// 加载传统的 MiniGameConfig.asset 配置
        /// </summary>
        private static void LoadLegacyConfig(List<BuildProfileInfo> buildProfiles)
        {
            try
            {
                // 加载传统MiniGameConfig.asset
                var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/TapTapMiniGame/Editor/MiniGameConfig.asset");
                if (config != null)
                {
                    // 通过反射获取DST路径
                    var bindingFlags = System.Reflection.BindingFlags.Instance | 
                                      System.Reflection.BindingFlags.NonPublic | 
                                      System.Reflection.BindingFlags.Public;
                                      
                    var projectConfField = config.GetType().GetField("ProjectConf", bindingFlags);
                    if (projectConfField != null)
                    {
                        var projectConf = projectConfField.GetValue(config);
                        var dstField = projectConf.GetType().GetField("DST", bindingFlags);
                        if (dstField != null)
                        {
                            string dst = (string)dstField.GetValue(projectConf);
                            if (!string.IsNullOrEmpty(dst))
                            {
                                // 检查是否已存在相同路径的配置
                                bool exists = buildProfiles.Any(p => p.dstPath == dst);
                                if (!exists)
                                {
                                    buildProfiles.Add(new BuildProfileInfo("Legacy Config", "Assets/TapTapMiniGame/Editor/MiniGameConfig.asset", dst, "TapTap"));
                                    Debug.Log($"Found Legacy Config: {dst}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading legacy config: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 构建配置信息
    /// </summary>
    [Serializable]
    public class BuildProfileInfo
    {
        public string profileName;
        public string profilePath;
        public string dstPath;
        public string hostName;
        
        public BuildProfileInfo(string name, string path, string dst, string host)
        {
            profileName = name;
            profilePath = path;
            dstPath = dst;
            hostName = host;
        }
    }
    
    /// <summary>
    /// 游戏信息数据结构
    /// </summary>
    [Serializable]
    public class GameInfo
    {
        public string appId;
        public string productName;
        public string companyName;
        public string productVersion;
        public string convertScriptVersion;
        public string convertToolVersion;
    }
}
#endif

