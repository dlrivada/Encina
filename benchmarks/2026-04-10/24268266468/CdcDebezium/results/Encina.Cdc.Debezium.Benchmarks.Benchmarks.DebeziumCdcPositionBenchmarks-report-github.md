```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.914 ns | 0.1269 ns | 0.0070 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 48.600 ns | 5.6306 ns | 0.3086 ns |  7.03 |    0.04 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 41.933 ns | 2.5011 ns | 0.1371 ns |  6.06 |    0.02 | 0.0091 |     152 B |        6.33 |
