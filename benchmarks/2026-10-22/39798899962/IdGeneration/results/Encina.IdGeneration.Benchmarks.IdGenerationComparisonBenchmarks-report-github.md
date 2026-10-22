```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   242.3 ns |  0.06 ns | 0.04 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     |   915.4 ns |  2.34 ns | 1.55 ns |  3.78 |    0.01 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     |   911.8 ns |  1.07 ns | 0.71 ns |  3.76 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,156.7 ns |  4.83 ns | 3.20 ns |  4.77 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   590.9 ns | 10.71 ns | 7.08 ns |  2.44 |    0.03 |    2 |      - |         - |          NA |
|                    |            |                |             |            |          |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   242.3 ns |  0.87 ns | 0.05 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           |   910.6 ns | 48.07 ns | 2.63 ns |  3.76 |    0.01 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           |   903.3 ns | 43.97 ns | 2.41 ns |  3.73 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,149.8 ns | 38.80 ns | 2.13 ns |  4.74 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   585.8 ns | 15.76 ns | 0.86 ns |  2.42 |    0.00 |    2 |      - |         - |          NA |
