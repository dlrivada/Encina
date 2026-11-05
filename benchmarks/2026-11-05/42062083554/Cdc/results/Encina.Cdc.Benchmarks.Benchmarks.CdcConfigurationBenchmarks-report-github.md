```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.35GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 255.44 ns | 74.63 ns | 4.091 ns |  3.74 |    0.07 | 0.0582 |     976 B |        2.60 |
| BuildConfigurationFluentChain |  68.32 ns | 20.49 ns | 1.123 ns |  1.00 |    0.02 | 0.0224 |     376 B |        1.00 |
