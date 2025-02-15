﻿using System;
using System.Collections.Generic;
using Utils;

namespace SimpleContainer.Container
{
    public class InjectionContainer : Injector, IInjectionContainer
    {
        /// <summary>
        /// 默认实例化模式
        /// </summary>
        protected const ResolutionMode DEFAULT_RESOLUTION_MODE = ResolutionMode.ALWAYS_RESOLVE;

        /// <summary>
        /// load 时是否摧毁容器
        /// </summary>
        public bool destroyOnLoad { get; set; }

        /// <summary>
        /// 容器 id
        /// </summary>
        public object id { get; private set; }

        /// <summary>
        /// 是否已经初始化
        /// </summary>
        private bool isInitialized;

        /// <summary>
        /// 容器 IContainerAOT 接口 list
        /// </summary>
        private List<IContainerAOT> aots;

        #region constructor

        public InjectionContainer()
            : base(new ReflectionCache(), new Binder(), DEFAULT_RESOLUTION_MODE)
        {
            RegisterSelf();
        }

        public InjectionContainer(object id)
            : base(new ReflectionCache(), new Binder(), DEFAULT_RESOLUTION_MODE)
        {
            this.id = id;
            RegisterSelf();
        }

        public InjectionContainer(IReflectionCache cache)
            : base(cache, new Binder(), DEFAULT_RESOLUTION_MODE)
        {
            RegisterSelf();
        }

        public InjectionContainer(ResolutionMode resolutionMode)
            : base(new ReflectionCache(), new Binder(), resolutionMode)
        {
            RegisterSelf();
        }

        public InjectionContainer(object id, IReflectionCache cache)
            : base(cache, new Binder(), DEFAULT_RESOLUTION_MODE)
        {
            this.id = id;
            RegisterSelf();
        }

        public InjectionContainer(IReflectionCache cache, ResolutionMode resolutionMode)
            : base(cache, new Binder(), resolutionMode)
        {
            RegisterSelf();
        }

        public InjectionContainer(IReflectionCache cache, IBinder binder)
            : base(cache, binder, DEFAULT_RESOLUTION_MODE)
        {
            RegisterSelf();
        }

        public InjectionContainer(object id, IReflectionCache cache, IBinder binder)
            : base(cache, binder, DEFAULT_RESOLUTION_MODE)
        {
            this.id = id;
            RegisterSelf();
        }

        public InjectionContainer(
            object id,
            IReflectionCache cache,
            IBinder binder,
            ResolutionMode resolutionMode)
            : base(cache, binder, resolutionMode)
        {
            this.id = id;
            RegisterSelf();
        }

        public InjectionContainer(object identifier, ResolutionMode resolutionMode)
            : this(identifier, new ReflectionCache(), new Binder(), resolutionMode)
        {
        }

        #endregion

        public void Init()
        {
            if (isInitialized)
            {
                return;
            }

            cache.CacheFromBinder(this);

            if (aots != null)
            {
                for (int i = 0; i < aots.Count; i++)
                {
                    aots[i].Init(this);
                }
            }

            isInitialized = true;
        }

        #region IDisposable implementation 

        /// <summary>
        /// 清空所有缓存和 binder
        /// </summary>
        public void Dispose()
        {
            if (aots != null)
            {
                for (int i = 0; i < aots.Count; i++)
                {
                    aots[i].OnUnregister(this);
                }
            }
            cache = null;
            binder = null;
        }

        #endregion

        #region IInjectionContainer implementation 

        /// <summary>
        /// 实例化指定类型的容器扩展并注入到 AOT list 
        /// </summary>
        virtual public IInjectionContainer RegisterAOT<T>() where T : IContainerAOT
        {
            RegisterAOT(Resolve<T>());

            return this;
        }

        /// <summary>
        /// 注册指定类型的容器扩展实例到 IContainerAOT list，并执行容器的 OnRegister 方法
        /// </summary>
        virtual public IInjectionContainer RegisterAOT(IContainerAOT aot)
        {
            // 如果 List<IContainerAOT> aots 为空,将其初始化
            if (aots == null) { aots = new List<IContainerAOT>(); }
            // 在之前未进行过添加的情况下才添加参数到 list
            if (GetAOT(aot.GetType()) == null)
            {
                aots.Add(aot);
                // 执行 OnRegister 方法
                aot.OnRegister(this);
            }

            return this;
        }

        /// <summary>
        /// 将所有指定类型的容器从 IContainerAOT list 中移除 
        /// </summary>
        virtual public IInjectionContainer UnregisterAOT<T>() where T : IContainerAOT
        {
            // 获取list 中所有指定类型的容器 aots 接口对象
            var AOTToUnregister = aots.OfTheType<T, IContainerAOT>();

            // 注销所有获取到的容器 aots 接口对象
            int length = AOTToUnregister.Count;
            for (int i = 0; i < length; i++)
            {
                UnregisterAOT(AOTToUnregister[i]);
            }

            return this;
        }

        /// <summary>
        /// 将一个容器扩展从 IContainerAOT list 中移除 
        /// </summary>
        virtual public IInjectionContainer UnregisterAOT(IContainerAOT aot)
        {
            if (!aots.Contains(aot)) { return this; }

            aots.Remove(aot);
            aot.OnUnregister(this);

            return this;
        }

        /// <summary>
        /// 从 aots list 中获取指定容器
        /// </summary>
        virtual public T GetAOT<T>() where T : IContainerAOT
        {
            return (T) GetAOT(typeof(T));
        }

        /// <summary>
        /// 从 aots list 中获取指定容器
        /// </summary>
        virtual public IContainerAOT GetAOT(Type type)
        {
            if (aots != null)
            {
                for (int i = 0; i < aots.Count; i++)
                {
                    if (aots[i].GetType().Equals(type))
                    {
                        return aots[i];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 反回 aots list 中是否含有指定容器
        /// </summary>
        virtual public bool HasAOT<T>()
        {
            return HasAOT(typeof(T));
        }

        /// <summary>
        /// 反回 aots list 中是否含有指定容器
        /// </summary>
        virtual public bool HasAOT(Type type)
        {
            return GetAOT(type) != null;
        }

        #endregion

        #region binder AOT event

        virtual public event BindingAddedHandler beforeAddBinding
        {
            add { binder.beforeAddBinding += value; }
            remove { binder.beforeAddBinding -= value; }
        }

        virtual public event BindingAddedHandler afterAddBinding
        {
            add { binder.afterAddBinding += value; }
            remove { binder.afterAddBinding -= value; }
        }

        virtual public event BindingRemovedHandler beforeRemoveBinding
        {
            add { binder.beforeRemoveBinding += value; }
            remove { binder.beforeRemoveBinding -= value; }
        }

        virtual public event BindingRemovedHandler afterRemoveBinding
        {
            add { binder.afterRemoveBinding += value; }
            remove { binder.afterRemoveBinding -= value; }
        }

        #endregion

        #region binder function

        #region Bind

        /// <summary>
        /// 返回一个指定 type 属性的新 Binding 实例，BindingType 为 ADDRESS，值约束为 MULTIPLE
        /// </summary>
        virtual public IBinding Bind<T>()
        {
            return binder.Bind<T>();
        }

        /// <summary>
        /// 返回一个指定 type 属性的新 Binding 实例，BindingType 为 ADDRESS，值约束为 MULTIPLE
        /// </summary>
        virtual public IBinding Bind(Type type)
        {
            return binder.Bind(type);
        }

        /// <summary>
        /// 返回一个指定 type 属性的新 Binding 实例，BindingType 为 SINGLETON，值约束为 SINGLE
        /// </summary>
        virtual public IBinding BindSingleton<T>()
        {
            return binder.BindSingleton<T>();
        }

        /// <summary>
        /// 返回一个指定 type 属性的新 Binding 实例，BindingType 为 SINGLETON，值约束为 SINGLE
        /// </summary>
        virtual public IBinding BindSingleton(Type type)
        {
            return binder.BindSingleton(type);
        }

        /// <summary>
        /// 返回一个指定 type 属性的新 Binding 实例，BindingType 属性为 FACTORY，值约束为 SINGLE
        /// </summary>
        virtual public IBinding BindFactory<T>()
        {
            return binder.BindFactory<T>();
        }

        /// <summary>
        /// 返回一个指定 type 属性的新 Binding 实例，BindingType 属性为 FACTORY，值约束为 SINGLE
        /// </summary>
        virtual public IBinding BindFactory(Type type)
        {
            return binder.BindFactory(type);
        }

        /// <summary>
        /// 返回一个指定 type 属性的新 Binding 实例，BindingType 为 MULTITON，值约束为 MULTIPLE
        /// </summary>
        virtual public IBinding BindMultiton<T>()
        {
            return binder.BindMultiton<T>();
        }

        /// <summary>
        /// 返回一个指定 type 属性的新 Binding 实例，BindingType 为 MULTITON，值约束为 MULTIPLE
        /// </summary>
        virtual public IBinding BindMultiton(Type type)
        {
            return binder.BindMultiton(type);
        }

        /// <summary>
        /// 创建多个指定类型的 binding，并返回 IBindingFactory
        /// </summary>
        virtual public IBindingFactory MultipleBind(Type[] types, BindingType[] bindingTypes)
        {
            return binder.MultipleBind(types, bindingTypes);
        }

        #endregion

        #region GetBinding

        /// <summary>
        /// 根据类型和id获取储存容器中的 Binding
        /// </summary>
        virtual public IBinding GetBinding<T>(object id)
        {
            return binder.GetBinding(typeof(T), id);
        }

        /// <summary>
        /// 根据类型和id获取储存容器中的 Binding
        /// </summary>
        virtual public IBinding GetBinding(Type type, object id)
        {
            return binder.GetBinding(type, id);
        }

        /// <summary>
        /// 根据类型获取储存容器中的所有同类型 Binding
        /// </summary>
        virtual public IList<IBinding> GetTypes<T>()
        {
            return binder.GetTypes(typeof(T));
        }

        /// <summary>
        /// 根据类型获取储存容器中的所有同类型 Binding
        /// </summary>
        virtual public IList<IBinding> GetTypes(Type type)
        {
            return binder.GetTypes(type);
        }

        /// <summary>
        /// 获取 binder 中所有的 Binding
        /// </summary>
        virtual public IList<IBinding> GetAll()
        {
            return binder.GetAll();
        }

        /// <summary>
        /// 根据是否符合委托的内容获取储存容器中的 Binding
        /// </summary>
		virtual public IList<IBinding> GetByDelegate(IsBindingAccordHandler isBindingAccordHandler)
        {
            return binder.GetByDelegate(isBindingAccordHandler);
        }

        /// <summary>
        /// 根据类型和是否符合委托的内容获取储存容器中的 Binding
        /// </summary>
		virtual public IList<IBinding> GetByDelegate<T>(IsBindingAccordHandler isBindingAccordHandler)
        {
            return binder.GetByDelegate(typeof(T), isBindingAccordHandler);
        }

        /// <summary>
        /// 根据类型和是否符合委托的内容获取储存容器中的 Binding
        /// </summary>
		virtual public IList<IBinding> GetByDelegate(Type type, IsBindingAccordHandler isBindingAccordHandler)
        {
            return binder.GetByDelegate(type, isBindingAccordHandler);
        }

        #endregion

        #region Unbind

        /// <summary>
        /// 根据类型从容器中删除所有同类型 Binding
        /// </summary>
        virtual public void UnbindType<T>()
        {
            binder.UnbindType<T>();
        }

        /// <summary>
        /// 根据类型从容器中删除所有同类型 Binding
        /// </summary>
        virtual public void UnbindType(Type type)
        {
            binder.UnbindType(type);
        }

        /// <summary>
        /// 从容器中删除值或 condition 与 instance 相同的 binding
        /// </summary>
        virtual public void UnbindInstance(object instance)
        {
            binder.UnbindInstance(instance);
        }

        /// <summary>
        /// 从容器中删除值或 condition 与 instance 相同的 binding
        /// </summary>
        virtual public void UnbindTag(string tag)
        {
            binder.UnbindTag(tag);
        }

        /// <summary>
        /// 根据委托从容器中删除 binding
        /// </summary>
        virtual public void UnbindByDelegate(IsBindingAccordHandler isBindingAccordHandler)
        {
            binder.UnbindByDelegate(isBindingAccordHandler);
        }

        /// <summary>
        /// 根据类型和委托从容器中删除 binding
        /// </summary>
        virtual public void UnbindByDelegate<T>(IsBindingAccordHandler isBindingAccordHandler)
        {
            binder.UnbindByDelegate(typeof(T), isBindingAccordHandler);
        }

        /// <summary>
        /// 根据类型和委托从容器中删除 binding
        /// </summary>
        virtual public void UnbindByDelegate(Type type, IsBindingAccordHandler isBindingAccordHandler)
        {
            binder.UnbindByDelegate(type, isBindingAccordHandler);
        }

        /// <summary>
        /// 根据类型和 id 从容器中删除 Binding
        /// </summary>
		virtual public void Unbind<T>(object id)
        {
            binder.Unbind<T>(id);
        }

        /// <summary>
        /// 根据类型和 id 从容器中删除 Binding
        /// </summary>
		virtual public void Unbind(Type type, object id)
        {
            binder.Unbind(type, id);
        }

        /// <summary>
        /// 根据 binding 从容器中删除 Binding
        /// </summary>
        virtual public void Unbind(IBinding binding)
        {
            binder.Unbind(binding);
        }

        #endregion

        /// <summary>
        /// 储存 binding
        /// </summary>
        public void Storing(IBinding binding)
        {
            binder.Storing(binding);
        }

        #endregion

        /// <summary>
        /// 绑定一个单例 binding，其 type 为自身类型，自身作为实例保存到 value
        /// </summary>
        virtual protected void RegisterSelf()
        {
            if (id != null) { BindSingleton<IInjectionContainer>().To(this).As(id); }
            else { BindSingleton<IInjectionContainer>().To(this); }
        }
    }
}