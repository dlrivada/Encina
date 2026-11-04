```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 433.2 ns | 0.29 ns | 0.17 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 327.4 ns | 0.47 ns | 0.24 ns |  0.76 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 433.0 ns | 0.25 ns | 0.15 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |         |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 436.7 ns | 6.92 ns | 0.38 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 326.8 ns | 8.00 ns | 0.44 ns |  0.75 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 435.5 ns | 3.68 ns | 0.20 ns |  1.00 |         - |          NA |
