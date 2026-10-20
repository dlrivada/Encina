```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,148.0 ns |   4.34 ns |  2.58 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   613.8 ns |   1.04 ns |  0.62 ns |  0.53 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,148.6 ns |   3.66 ns |  2.42 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |           |          |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,163.1 ns | 211.84 ns | 11.61 ns |  0.94 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   612.0 ns |  21.49 ns |  1.18 ns |  0.49 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,238.1 ns |  58.58 ns |  3.21 ns |  1.00 |         - |          NA |
