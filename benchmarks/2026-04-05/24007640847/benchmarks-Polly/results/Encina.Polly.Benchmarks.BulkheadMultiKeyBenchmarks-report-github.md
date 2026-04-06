```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-SWDXLI : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                    | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | KeyCount | Mean          | Error     | StdDev    | Median        | Allocated |
|-------------------------- |----------- |---------------- |--------------- |------------ |------------ |------------ |--------- |--------------:|----------:|----------:|--------------:|----------:|
| **AcquireAcrossMultipleKeys** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **1**        |      **9.715 μs** | **88.383 μs** | **4.8445 μs** |      **6.923 μs** |     **672 B** |
| AcquireAcrossMultipleKeys | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 1        | 10,531.478 μs |        NA | 0.0000 μs | 10,531.478 μs |     672 B |
| **AcquireAcrossMultipleKeys** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **10**       |     **13.210 μs** | **33.487 μs** | **1.8355 μs** |     **12.448 μs** |    **6720 B** |
| AcquireAcrossMultipleKeys | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10       | 10,710.401 μs |        NA | 0.0000 μs | 10,710.401 μs |    6720 B |
| **AcquireAcrossMultipleKeys** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **100**      |     **59.547 μs** | **12.005 μs** | **0.6581 μs** |     **59.441 μs** |   **67200 B** |
| AcquireAcrossMultipleKeys | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 100      | 10,631.925 μs |        NA | 0.0000 μs | 10,631.925 μs |   67200 B |
