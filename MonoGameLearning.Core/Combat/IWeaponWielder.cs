namespace MonoGameLearning.Core.Combat;

public interface IWeaponWielder
{
    void EquipWeapon(WeaponDef weapon);
    void UnequipWeapon();
}