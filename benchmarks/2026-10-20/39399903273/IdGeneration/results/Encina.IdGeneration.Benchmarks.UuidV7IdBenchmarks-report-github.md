```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 843.6 ns |   7.23 ns | 3.78 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 394.2 ns |   2.10 ns | 1.25 ns |  0.47 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 844.4 ns |  12.29 ns | 8.13 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |           |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 855.3 ns |  80.48 ns | 4.41 ns |  1.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 391.0 ns | 122.88 ns | 6.74 ns |  0.47 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 840.9 ns |  75.26 ns | 4.13 ns |  1.00 |         - |          NA |
