```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   242.1 ns |   0.09 ns | 0.05 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 1,239.1 ns |  12.94 ns | 7.70 ns |  5.12 |    0.03 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 1,137.8 ns |   2.43 ns | 1.45 ns |  4.70 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,503.7 ns |   4.38 ns | 2.89 ns |  6.21 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   608.9 ns |   0.68 ns | 0.36 ns |  2.52 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |            |           |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   242.0 ns |   1.63 ns | 0.09 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 1,158.6 ns | 102.44 ns | 5.62 ns |  4.79 |    0.02 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 1,226.6 ns |  65.34 ns | 3.58 ns |  5.07 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,401.8 ns |  83.70 ns | 4.59 ns |  5.79 |    0.02 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   609.1 ns |   3.48 ns | 0.19 ns |  2.52 |    0.00 |    2 |      - |         - |          NA |
