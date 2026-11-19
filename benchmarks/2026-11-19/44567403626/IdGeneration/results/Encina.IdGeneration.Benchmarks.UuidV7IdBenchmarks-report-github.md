```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 549.6 ns |   5.43 ns |  2.84 ns |  1.00 |    0.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 469.3 ns |  10.85 ns |  7.17 ns |  0.85 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 549.9 ns |   7.92 ns |  4.71 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 564.7 ns | 317.23 ns | 17.39 ns |  1.06 |    0.03 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 466.6 ns |  34.55 ns |  1.89 ns |  0.87 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 534.0 ns | 189.84 ns | 10.41 ns |  1.00 |    0.02 |         - |          NA |
