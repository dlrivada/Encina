```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.37GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 549.2 ns |  18.59 ns | 12.29 ns |  1.05 |    0.03 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 439.9 ns |   5.44 ns |  3.60 ns |  0.84 |    0.02 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 522.1 ns |  21.28 ns | 14.08 ns |  1.00 |    0.04 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 519.7 ns | 158.19 ns |  8.67 ns |  1.00 |    0.03 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 449.7 ns |  52.54 ns |  2.88 ns |  0.86 |    0.02 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 520.2 ns | 259.48 ns | 14.22 ns |  1.00 |    0.03 |         - |          NA |
