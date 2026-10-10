```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,174.9 ns |     2.45 ns |  1.28 ns |  0.98 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   722.3 ns |     4.21 ns |  2.50 ns |  0.60 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,200.8 ns |    39.41 ns | 26.07 ns |  1.00 |    0.03 |         - |          NA |
|                    |            |                |             |            |             |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,173.9 ns |    64.27 ns |  3.52 ns |  0.95 |    0.04 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   718.3 ns |    16.72 ns |  0.92 ns |  0.58 |    0.03 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,232.3 ns | 1,183.34 ns | 64.86 ns |  1.00 |    0.06 |         - |          NA |
