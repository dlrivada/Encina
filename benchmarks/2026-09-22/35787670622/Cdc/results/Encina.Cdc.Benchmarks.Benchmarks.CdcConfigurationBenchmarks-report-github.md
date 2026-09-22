```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                        | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------ |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddEncinaCdc_Registration     | 460.5 ns | 127.72 ns | 7.00 ns |  3.63 |    0.06 | 0.0582 |     976 B |        2.60 |
| BuildConfigurationFluentChain | 126.8 ns |  25.77 ns | 1.41 ns |  1.00 |    0.01 | 0.0224 |     376 B |        1.00 |
