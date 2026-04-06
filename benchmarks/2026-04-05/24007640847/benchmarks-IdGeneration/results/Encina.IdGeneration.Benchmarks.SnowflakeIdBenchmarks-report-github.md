```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method               | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|--------:|--------:|------:|----------:|------------:|
| Generate             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        242.0 ns | 0.06 ns | 0.04 ns |  1.00 |         - |          NA |
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        242.1 ns | 0.08 ns | 0.05 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        240.5 ns | 0.10 ns | 0.06 ns |  0.99 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        242.0 ns | 0.10 ns | 0.07 ns |  1.00 |         - |          NA |
|                      |            |                |             |             |              |             |                 |         |         |       |           |             |
| Generate             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,677,333.0 ns |      NA | 0.00 ns |  1.00 |         - |          NA |
| Generate_WithShardId | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 18,159,616.0 ns |      NA | 0.00 ns |  1.43 |         - |          NA |
| ExtractShardId       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,553,741.0 ns |      NA | 0.00 ns |  1.62 |         - |          NA |
| GenerateAndGetValue  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 13,223,520.0 ns |      NA | 0.00 ns |  1.04 |         - |          NA |
