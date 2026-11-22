```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 746.7 ns |  7.40 ns |  4.90 ns | 745.5 ns |  0.99 |    0.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           | 665.4 ns |  0.72 ns |  0.38 ns | 665.4 ns |  0.89 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 750.6 ns |  7.56 ns |  5.00 ns | 749.2 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |             |          |          |          |          |       |         |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 764.2 ns | 15.54 ns | 22.28 ns | 764.4 ns |  1.00 |    0.04 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          | 666.0 ns |  2.41 ns |  3.30 ns | 668.2 ns |  0.87 |    0.02 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 763.7 ns | 14.24 ns | 20.42 ns | 779.1 ns |  1.00 |    0.04 |         - |          NA |
