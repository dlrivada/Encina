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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 725.3 ns |  21.58 ns | 14.27 ns |  0.98 |    0.03 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 452.5 ns |   6.92 ns |  4.12 ns |  0.61 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 736.7 ns |  25.24 ns | 16.70 ns |  1.00 |    0.03 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 727.3 ns | 372.83 ns | 20.44 ns |  0.96 |    0.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 445.0 ns |  72.30 ns |  3.96 ns |  0.59 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 754.6 ns |  66.68 ns |  3.65 ns |  1.00 |    0.01 |         - |          NA |
