```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]     : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3


```
| Method               | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 3           | 241.9 ns | 0.09 ns | 0.06 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 3           | 241.9 ns | 0.11 ns | 0.07 ns |  1.00 |         - |          NA |
| Generate             | Job-YFEFPZ | 10             | Default     | 3           | 241.9 ns | 0.10 ns | 0.07 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 3           | 240.3 ns | 0.12 ns | 0.06 ns |  0.99 |         - |          NA |
|                      |            |                |             |             |          |         |         |       |           |             |
| Generate_WithShardId | MediumRun  | 15             | 2           | 10          | 241.9 ns | 0.04 ns | 0.05 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | MediumRun  | 15             | 2           | 10          | 241.9 ns | 0.03 ns | 0.05 ns |  1.00 |         - |          NA |
| Generate             | MediumRun  | 15             | 2           | 10          | 241.9 ns | 0.04 ns | 0.05 ns |  1.00 |         - |          NA |
| ExtractShardId       | MediumRun  | 15             | 2           | 10          | 240.2 ns | 0.11 ns | 0.16 ns |  0.99 |         - |          NA |
