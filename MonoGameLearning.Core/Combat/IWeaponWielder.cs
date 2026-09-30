namespace MonoGameLearning.Core.Combat;

public interface IWeaponWielder
{
    WeaponDef? EquippedWeapon { get; }

    void EquipWeapon(WeaponDef weapon);
    void UnequipWeapon();
}