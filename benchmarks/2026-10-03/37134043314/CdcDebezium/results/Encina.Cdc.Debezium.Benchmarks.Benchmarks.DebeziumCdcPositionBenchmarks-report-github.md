```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.852 ns |  2.4788 ns | 0.1359 ns |  1.00 |    0.02 | 0.0003 |      24 B |        1.00 |
| FromBytes      | 64.408 ns | 19.9451 ns | 1.0933 ns |  8.20 |    0.17 | 0.0036 |     304 B |       12.67 |
| ToBytes        | 50.129 ns |  0.8442 ns | 0.0463 ns |  6.39 |    0.09 | 0.0018 |     152 B |        6.33 |
