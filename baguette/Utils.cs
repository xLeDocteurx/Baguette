using System;
using System.Reflection.Metadata;

//UIntPtr ResolveHandle(const CHandle& driver, UIntPtr entityList, uint handle)
//{
//    if (!entityList || handle == 0 || handle == 0xFFFFFFFF)
//        return 0;
//    const auto listEntry =
//        driver::read_memory<uintptr_t>(driver, entityList + 0x8 * ((handle & 0x7FFF) >> 9) + 0x10);
//    if (!listEntry)
//        return 0;
//    return driver::read_memory<uintptr_t>(driver, listEntry + 0x78 * (handle & 0x1FF));
//}


//uintptr_t ResolveHandle(uintptr_t entityList, uint32_t handle)
//{
//    if (!entityList || handle == 0 || handle == 0xFFFFFFFF)
//        return 0;
//const auto listEntry = deref(uintptr_t, entityList + 0x8 * ((handle & 0x7FFF) >> 9) + 0x10);
//if (!listEntry)
//    return 0;
//return deref(uintptr_t, listEntry + 0x78 * (handle & 0x1FF));
//}