```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 650.6 ns |   3.75 ns |  2.48 ns |  1.00 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 689.9 ns |   4.20 ns |  2.50 ns |  1.06 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 649.2 ns |   2.42 ns |  1.44 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 652.3 ns |  15.58 ns |  0.85 ns |  0.99 |    0.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 707.2 ns | 435.98 ns | 23.90 ns |  1.07 |    0.03 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 660.4 ns |  36.39 ns |  1.99 ns |  1.00 |    0.00 |         - |          NA |
