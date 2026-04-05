```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-SWDXLI : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                    | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | KeyCount | Mean          | Error      | StdDev    | Allocated |
|-------------------------- |----------- |---------------- |--------------- |------------ |------------ |------------ |--------- |--------------:|-----------:|----------:|----------:|
| **AcquireAcrossMultipleKeys** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **1**        |      **8.463 μs** |   **5.437 μs** | **0.2980 μs** |     **672 B** |
| AcquireAcrossMultipleKeys | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 1        | 10,673.387 μs |         NA | 0.0000 μs |     672 B |
| **AcquireAcrossMultipleKeys** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **10**       |     **12.257 μs** |   **6.977 μs** | **0.3824 μs** |    **6720 B** |
| AcquireAcrossMultipleKeys | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10       | 11,021.689 μs |         NA | 0.0000 μs |    6720 B |
| **AcquireAcrossMultipleKeys** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **100**      |     **62.676 μs** | **130.713 μs** | **7.1648 μs** |   **67200 B** |
| AcquireAcrossMultipleKeys | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 100      | 10,863.011 μs |         NA | 0.0000 μs |   67200 B |
