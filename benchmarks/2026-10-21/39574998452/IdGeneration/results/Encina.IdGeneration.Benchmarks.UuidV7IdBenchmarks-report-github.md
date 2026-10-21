```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 919.1 ns |  1.55 ns | 0.92 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 558.2 ns |  3.07 ns | 1.83 ns |  0.61 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 917.3 ns |  2.53 ns | 1.51 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 975.8 ns | 57.64 ns | 3.16 ns |  1.07 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 558.3 ns | 88.03 ns | 4.82 ns |  0.61 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 909.8 ns |  4.14 ns | 0.23 ns |  1.00 |         - |          NA |
