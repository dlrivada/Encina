```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 670.8 ns |  17.06 ns | 11.28 ns |  0.94 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 425.1 ns |   5.17 ns |  3.08 ns |  0.60 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 710.8 ns |  21.34 ns | 12.70 ns |  1.00 |    0.02 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 673.9 ns | 645.02 ns | 35.36 ns |  1.00 |    0.05 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 421.1 ns |  31.13 ns |  1.71 ns |  0.63 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 673.5 ns | 308.09 ns | 16.89 ns |  1.00 |    0.03 |         - |          NA |
