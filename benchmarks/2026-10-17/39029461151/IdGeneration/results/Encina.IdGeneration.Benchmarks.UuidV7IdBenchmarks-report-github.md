```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,131.0 ns |  2.43 ns | 1.44 ns |  0.98 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   613.2 ns |  1.21 ns | 0.72 ns |  0.53 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,152.0 ns |  3.29 ns | 2.18 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,139.8 ns | 80.48 ns | 4.41 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   611.5 ns | 61.87 ns | 3.39 ns |  0.53 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,151.4 ns | 28.25 ns | 1.55 ns |  1.00 |         - |          NA |
