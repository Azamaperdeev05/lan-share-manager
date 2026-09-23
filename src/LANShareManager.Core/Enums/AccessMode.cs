namespace LANShareManager.Core.Enums;

/// <summary>
/// Режимы доступа к общему ресурсу (SMB и NTFS).
/// </summary>
public enum AccessMode
{
    /// <summary>
    /// Только чтение: SMB (Everyone -> Read), NTFS (Everyone -> Read & Execute)
    /// </summary>
    ReadOnly,

    /// <summary>
    /// Чтение и запись: SMB (Everyone -> Change), NTFS (Everyone -> Modify)
    /// </summary>
    ReadWrite,

    /// <summary>
    /// Полный доступ: SMB (Everyone -> Full), NTFS (Everyone -> Full Control)
    /// </summary>
    FullControl
}
