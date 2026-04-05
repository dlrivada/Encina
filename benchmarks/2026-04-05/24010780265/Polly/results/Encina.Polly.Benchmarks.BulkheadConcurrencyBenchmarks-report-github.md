```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-SWDXLI : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                      | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | ConcurrentRequests | Mean         | Error     | StdDev    | Allocated |
|---------------------------- |----------- |---------------- |--------------- |------------ |------------ |------------ |------------------- |-------------:|----------:|----------:|----------:|
| **ConcurrentAcquireAndRelease** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **10**                 |     **39.70 μs** |  **30.16 μs** |  **1.653 μs** |    **7.3 KB** |
| ConcurrentAcquireAndRelease | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10                 | 18,312.42 μs |        NA |  0.000 μs |    7.3 KB |
| **ConcurrentAcquireAndRelease** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **50**                 |     **77.01 μs** | **299.50 μs** | **16.416 μs** |  **34.39 KB** |
| ConcurrentAcquireAndRelease | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 50                 | 18,116.47 μs |        NA |  0.000 μs |  34.39 KB |
| **ConcurrentAcquireAndRelease** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **100**                |    **143.28 μs** | **641.02 μs** | **35.136 μs** |  **68.38 KB** |
| ConcurrentAcquireAndRelease | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 100                | 17,999.17 μs |        NA |  0.000 μs |  68.38 KB |
