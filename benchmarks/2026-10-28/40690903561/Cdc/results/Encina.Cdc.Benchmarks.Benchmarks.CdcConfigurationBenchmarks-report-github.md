```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 243.52 ns | 29.81 ns | 1.634 ns |  3.50 |    0.10 | 0.0582 |     976 B |        2.60 |
| BuildConfigurationFluentChain |  69.72 ns | 41.23 ns | 2.260 ns |  1.00 |    0.04 | 0.0224 |     376 B |        1.00 |
