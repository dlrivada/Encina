```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.36GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 682.9 ns |  15.77 ns | 8.25 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 426.8 ns |  11.89 ns | 7.86 ns |  0.62 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 683.6 ns |   3.07 ns | 1.83 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 658.8 ns |  33.90 ns | 1.86 ns |  0.98 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 433.7 ns |  57.68 ns | 3.16 ns |  0.64 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 672.8 ns | 110.34 ns | 6.05 ns |  1.00 |         - |          NA |
