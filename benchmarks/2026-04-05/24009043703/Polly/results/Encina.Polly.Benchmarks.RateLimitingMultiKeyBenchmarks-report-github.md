```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-KDXSYM : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                    | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | KeyCount | Mean           | Error     | StdDev   | Gen0   | Allocated |
|-------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------- |---------------:|----------:|---------:|-------:|----------:|
| **AcquireAcrossMultipleKeys** | **Job-KDXSYM** | **3**              | **Default**     | **Default**     | **16**           | **2**           | **1**        |       **114.9 ns** |   **2.58 ns** |  **0.14 ns** | **0.0057** |      **96 B** |
| AcquireAcrossMultipleKeys | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1        | 6,529,543.0 ns |        NA |  0.00 ns |      - |      96 B |
| **AcquireAcrossMultipleKeys** | **Job-KDXSYM** | **3**              | **Default**     | **Default**     | **16**           | **2**           | **10**       |     **1,118.3 ns** |   **9.31 ns** |  **0.51 ns** | **0.0572** |     **960 B** |
| AcquireAcrossMultipleKeys | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 10       | 6,576,495.0 ns |        NA |  0.00 ns |      - |     960 B |
| **AcquireAcrossMultipleKeys** | **Job-KDXSYM** | **3**              | **Default**     | **Default**     | **16**           | **2**           | **100**      |    **11,003.3 ns** | **553.86 ns** | **30.36 ns** | **0.5646** |    **9600 B** |
| AcquireAcrossMultipleKeys | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 100      | 6,624,622.0 ns |        NA |  0.00 ns |      - |    9600 B |
