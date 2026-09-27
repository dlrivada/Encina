```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 437.9 ns | 16.84 ns | 0.92 ns |  3.72 |    0.02 | 0.0386 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 117.6 ns | 14.30 ns | 0.78 ns |  1.00 |    0.01 | 0.0148 |     376 B |        1.00 |
