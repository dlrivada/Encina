```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 430.6 ns | 21.93 ns | 1.20 ns |  3.60 | 0.0582 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 119.6 ns |  6.74 ns | 0.37 ns |  1.00 | 0.0224 |     376 B |        1.00 |
