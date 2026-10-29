```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.612 ns | 0.5502 ns | 0.0302 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 53.277 ns | 7.8938 ns | 0.4327 ns |  7.00 |    0.05 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 41.400 ns | 6.3086 ns | 0.3458 ns |  5.44 |    0.04 | 0.0091 |     152 B |        6.33 |
