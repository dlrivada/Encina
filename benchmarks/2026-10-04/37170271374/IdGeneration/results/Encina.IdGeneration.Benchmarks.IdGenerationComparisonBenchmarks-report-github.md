```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 3           |   242.0 ns |  0.10 ns | 0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 3           | 1,100.5 ns | 10.16 ns | 6.72 ns |  4.55 |    0.03 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 3           | 1,097.5 ns |  4.14 ns | 2.74 ns |  4.53 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 3           | 1,445.8 ns |  3.34 ns | 1.75 ns |  5.97 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 3           |   610.0 ns |  0.53 ns | 0.31 ns |  2.52 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |             |            |          |         |       |         |      |        |           |             |
| Snowflake          | MediumRun  | 15             | 2           | 10          |   242.1 ns |  0.03 ns | 0.05 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | MediumRun  | 15             | 2           | 10          | 1,109.2 ns |  2.81 ns | 4.21 ns |  4.58 |    0.02 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | MediumRun  | 15             | 2           | 10          | 1,095.9 ns |  3.33 ns | 4.67 ns |  4.53 |    0.02 |    3 |      - |         - |          NA |
| ShardPrefixed      | MediumRun  | 15             | 2           | 10          | 1,425.5 ns |  2.31 ns | 3.39 ns |  5.89 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | MediumRun  | 15             | 2           | 10          |   608.2 ns |  0.75 ns | 1.03 ns |  2.51 |    0.00 |    2 |      - |         - |          NA |
