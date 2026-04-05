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
| **AcquireAcrossMultipleKeys** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **1**        |      **7.430 μs** | **13.429 μs** | **0.7361 μs** |     **672 B** |
| AcquireAcrossMultipleKeys | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 1        | 10,431.260 μs |        NA | 0.0000 μs |     672 B |
| **AcquireAcrossMultipleKeys** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **10**       |     **11.929 μs** |  **9.123 μs** | **0.5001 μs** |    **6720 B** |
| AcquireAcrossMultipleKeys | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10       | 10,703.869 μs |        NA | 0.0000 μs |    6720 B |
| **AcquireAcrossMultipleKeys** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **100**      |     **57.042 μs** |  **9.329 μs** | **0.5113 μs** |   **67200 B** |
| AcquireAcrossMultipleKeys | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 100      | 10,598.692 μs |        NA | 0.0000 μs |   67200 B |
