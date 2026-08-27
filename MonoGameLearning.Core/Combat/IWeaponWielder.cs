namespace MonoGameLearning.Core.Combat;

public interface IWeaponWielder
{
    void EquipWeapon(MeleeWeaponDef weapon);
    void UnequipWeapon();
}