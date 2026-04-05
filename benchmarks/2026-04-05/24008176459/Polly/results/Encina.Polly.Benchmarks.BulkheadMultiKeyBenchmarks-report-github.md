```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-SWDXLI : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                    | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | KeyCount | Mean          | Error     | StdDev    | Allocated |
|-------------------------- |----------- |---------------- |--------------- |------------ |------------ |------------ |--------- |--------------:|----------:|----------:|----------:|
| **AcquireAcrossMultipleKeys** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **1**        |      **9.855 μs** | **58.627 μs** | **3.2136 μs** |     **672 B** |
| AcquireAcrossMultipleKeys | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 1        | 12,459.579 μs |        NA | 0.0000 μs |     672 B |
| **AcquireAcrossMultipleKeys** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **10**       |     **17.924 μs** | **28.489 μs** | **1.5616 μs** |    **6720 B** |
| AcquireAcrossMultipleKeys | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10       | 12,358.520 μs |        NA | 0.0000 μs |    6720 B |
| **AcquireAcrossMultipleKeys** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **100**      |     **61.071 μs** | **51.676 μs** | **2.8326 μs** |   **67200 B** |
| AcquireAcrossMultipleKeys | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 100      | 12,472.994 μs |        NA | 0.0000 μs |   67200 B |
