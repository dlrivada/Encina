```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,130.7 ns |   4.01 ns |  2.65 ns |  0.93 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   718.8 ns |   1.44 ns |  0.85 ns |  0.59 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,216.3 ns |   2.31 ns |  1.37 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |            |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,144.4 ns | 405.11 ns | 22.21 ns |  0.93 |    0.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   719.4 ns |  48.28 ns |  2.65 ns |  0.58 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,236.2 ns | 497.42 ns | 27.27 ns |  1.00 |    0.03 |         - |          NA |
