```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,123.8 ns |   5.25 ns | 3.47 ns |  0.93 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   612.6 ns |   1.14 ns | 0.68 ns |  0.51 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,202.3 ns |   3.63 ns | 1.90 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,150.6 ns | 144.27 ns | 7.91 ns |  0.95 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   612.5 ns |   2.70 ns | 0.15 ns |  0.51 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,208.7 ns | 100.18 ns | 5.49 ns |  1.00 |         - |          NA |
