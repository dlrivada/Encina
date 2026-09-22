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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 1,166.2 ns |  76.90 ns | 50.86 ns |  1.03 |    0.04 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     |   722.1 ns |  14.72 ns |  9.73 ns |  0.64 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 1,131.5 ns |  13.30 ns |  7.91 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |            |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 1,117.7 ns |  27.96 ns |  1.53 ns |  0.97 |    0.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           |   729.1 ns | 321.28 ns | 17.61 ns |  0.64 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 1,146.9 ns |  23.87 ns |  1.31 ns |  1.00 |    0.00 |         - |          NA |
