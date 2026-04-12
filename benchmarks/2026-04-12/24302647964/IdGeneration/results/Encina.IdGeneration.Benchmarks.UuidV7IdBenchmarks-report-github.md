```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 1,088.2 ns |  3.53 ns |  2.10 ns | 1,089.4 ns |  0.99 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           |   599.2 ns |  0.32 ns |  0.17 ns |   599.2 ns |  0.55 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 1,094.0 ns |  3.08 ns |  1.83 ns | 1,093.2 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |             |            |          |          |            |       |         |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 1,120.3 ns | 29.18 ns | 40.91 ns | 1,086.7 ns |  1.03 |    0.04 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          |   598.8 ns |  0.57 ns |  0.81 ns |   598.9 ns |  0.55 |    0.00 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 1,086.3 ns |  2.41 ns |  3.54 ns | 1,087.0 ns |  1.00 |    0.00 |         - |          NA |
