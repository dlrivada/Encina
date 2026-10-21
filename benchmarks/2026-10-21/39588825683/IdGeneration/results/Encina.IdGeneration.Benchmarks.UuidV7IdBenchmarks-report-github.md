```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,188.7 ns |   1.27 ns |  0.66 ns |  1.06 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   642.4 ns |   0.97 ns |  0.51 ns |  0.57 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,123.2 ns |   5.18 ns |  3.43 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |            |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,125.0 ns | 120.63 ns |  6.61 ns |  1.00 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   647.1 ns |   4.29 ns |  0.24 ns |  0.57 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,129.8 ns | 278.92 ns | 15.29 ns |  1.00 |    0.02 |         - |          NA |
