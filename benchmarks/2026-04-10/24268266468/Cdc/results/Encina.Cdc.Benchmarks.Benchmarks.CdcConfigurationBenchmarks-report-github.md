```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 470.5 ns | 11.65 ns | 0.64 ns |  3.80 | 0.0386 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 123.7 ns |  7.28 ns | 0.40 ns |  1.00 | 0.0148 |     376 B |        1.00 |
