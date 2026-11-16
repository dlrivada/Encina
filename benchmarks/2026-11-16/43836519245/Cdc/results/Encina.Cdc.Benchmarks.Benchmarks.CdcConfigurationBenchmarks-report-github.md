```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 357.8 ns | 40.08 ns | 2.20 ns |  3.42 |    0.06 | 0.0114 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 104.6 ns | 34.66 ns | 1.90 ns |  1.00 |    0.02 | 0.0044 |     376 B |        1.00 |
