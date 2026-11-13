```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.35GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 369.4 ns | 244.02 ns | 13.38 ns |  3.36 |    0.11 | 0.0114 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 110.1 ns |  23.32 ns |  1.28 ns |  1.00 |    0.01 | 0.0044 |     376 B |        1.00 |
