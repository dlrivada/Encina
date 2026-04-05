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
| **ConcurrentAcquireAndRelease** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **10**                 |     **57.04 μs** | **365.52 μs** | **20.036 μs** |    **7.3 KB** |
| ConcurrentAcquireAndRelease | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10                 | 20,285.38 μs |        NA |  0.000 μs |   7.34 KB |
| **ConcurrentAcquireAndRelease** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **50**                 |     **75.83 μs** | **499.76 μs** | **27.394 μs** |  **34.59 KB** |
| ConcurrentAcquireAndRelease | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 50                 | 21,998.60 μs |        NA |  0.000 μs |  34.39 KB |
| **ConcurrentAcquireAndRelease** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **100**                |    **227.59 μs** | **699.28 μs** | **38.330 μs** |  **68.38 KB** |
| ConcurrentAcquireAndRelease | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 100                | 21,061.91 μs |        NA |  0.000 μs |  68.38 KB |
