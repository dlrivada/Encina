```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 682.5 ns |  3.37 ns | 1.76 ns |  1.04 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 679.8 ns |  2.66 ns | 1.39 ns |  1.03 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 657.4 ns |  6.69 ns | 3.50 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 656.6 ns | 41.77 ns | 2.29 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 681.1 ns | 21.74 ns | 1.19 ns |  1.03 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 658.2 ns | 35.52 ns | 1.95 ns |  1.00 |         - |          NA |
