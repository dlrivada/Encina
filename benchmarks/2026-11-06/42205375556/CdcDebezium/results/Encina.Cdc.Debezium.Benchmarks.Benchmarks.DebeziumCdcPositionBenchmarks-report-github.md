```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  9.494 ns | 3.034 ns | 0.1663 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 48.779 ns | 6.792 ns | 0.3723 ns |  5.14 |    0.08 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 39.354 ns | 3.074 ns | 0.1685 ns |  4.15 |    0.06 | 0.0091 |     152 B |        6.33 |
