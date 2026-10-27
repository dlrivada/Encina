```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 688.1 ns |  23.66 ns | 15.65 ns |  0.97 |    0.03 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 447.7 ns |  17.38 ns | 11.49 ns |  0.63 |    0.02 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 710.8 ns |  16.07 ns | 10.63 ns |  1.00 |    0.02 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 691.6 ns |  34.24 ns |  1.88 ns |  1.01 |    0.03 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 435.9 ns |  65.03 ns |  3.56 ns |  0.63 |    0.02 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 687.3 ns | 363.91 ns | 19.95 ns |  1.00 |    0.04 |         - |          NA |
