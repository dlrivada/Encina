```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 370.80 ns | 76.21 ns | 4.177 ns |  3.82 |    0.04 | 0.0582 |     976 B |        2.60 |
| BuildConfigurationFluentChain |  97.12 ns | 12.16 ns | 0.667 ns |  1.00 |    0.01 | 0.0224 |     376 B |        1.00 |
