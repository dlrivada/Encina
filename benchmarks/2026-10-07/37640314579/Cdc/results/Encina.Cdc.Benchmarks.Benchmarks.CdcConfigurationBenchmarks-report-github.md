```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 470.4 ns | 82.67 ns | 4.53 ns |  3.71 |    0.07 | 0.0582 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 126.7 ns | 42.93 ns | 2.35 ns |  1.00 |    0.02 | 0.0224 |     376 B |        1.00 |
