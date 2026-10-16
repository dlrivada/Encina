```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 717.8 ns |   5.95 ns | 3.54 ns |  1.03 |    0.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 431.6 ns |   9.98 ns | 5.94 ns |  0.62 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 696.4 ns |  12.93 ns | 6.76 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |         |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 712.0 ns | 175.72 ns | 9.63 ns |  1.01 |    0.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 440.8 ns |  61.48 ns | 3.37 ns |  0.63 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 702.9 ns | 163.90 ns | 8.98 ns |  1.00 |    0.02 |         - |          NA |
