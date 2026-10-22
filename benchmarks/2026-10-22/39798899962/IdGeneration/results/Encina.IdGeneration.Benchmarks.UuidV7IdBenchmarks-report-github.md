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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,105.1 ns |  5.51 ns | 3.65 ns |  0.93 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   614.2 ns |  1.45 ns | 0.87 ns |  0.52 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,186.8 ns |  4.15 ns | 2.47 ns |  1.00 |         - |          NA |
|                    |            |                |             |            |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,183.3 ns | 37.91 ns | 2.08 ns |  1.05 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   612.2 ns | 19.52 ns | 1.07 ns |  0.55 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,122.6 ns | 41.40 ns | 2.27 ns |  1.00 |         - |          NA |
