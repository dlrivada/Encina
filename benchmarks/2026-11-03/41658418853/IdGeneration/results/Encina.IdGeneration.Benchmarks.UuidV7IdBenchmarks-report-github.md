```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 836.2 ns |   8.47 ns |  4.43 ns |  1.00 |    0.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 876.5 ns |   2.59 ns |  1.54 ns |  1.05 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 833.3 ns |   3.89 ns |  2.58 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 857.7 ns | 297.95 ns | 16.33 ns |  1.02 |    0.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 874.2 ns |  61.39 ns |  3.37 ns |  1.04 |    0.00 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 839.0 ns |  56.78 ns |  3.11 ns |  1.00 |    0.00 |         - |          NA |
