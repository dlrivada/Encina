```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.42GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 533.4 ns |  19.57 ns | 12.94 ns |  1.02 |    0.03 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 438.6 ns |   3.10 ns |  1.85 ns |  0.84 |    0.02 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 521.1 ns |  19.52 ns | 11.62 ns |  1.00 |    0.03 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 540.8 ns | 229.40 ns | 12.57 ns |  1.01 |    0.03 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 469.5 ns | 111.51 ns |  6.11 ns |  0.88 |    0.02 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 533.3 ns | 291.70 ns | 15.99 ns |  1.00 |    0.04 |         - |          NA |
