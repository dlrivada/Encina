```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 960.1 ns | 1.58 ns | 0.94 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           | 398.2 ns | 3.40 ns | 2.25 ns |  0.42 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 957.8 ns | 1.29 ns | 0.85 ns |  1.00 |         - |          NA |
|                    |            |                |             |             |          |         |         |       |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 955.5 ns | 1.51 ns | 2.21 ns |  1.00 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          | 393.0 ns | 0.44 ns | 0.64 ns |  0.41 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 955.8 ns | 2.07 ns | 2.97 ns |  1.00 |         - |          NA |
