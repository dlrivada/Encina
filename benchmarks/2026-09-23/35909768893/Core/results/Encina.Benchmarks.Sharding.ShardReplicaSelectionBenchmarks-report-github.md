```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| RoundRobin.SelectReplica       | DefaultJob | Default        | Default     | Default     |   5.679 ns | 0.1151 ns | 0.1076 ns |  1.00 |    0.03 |    1 |         - |          NA |
| Random.SelectReplica           | DefaultJob | Default        | Default     | Default     |  13.482 ns | 0.0139 ns | 0.0109 ns |  2.37 |    0.04 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | DefaultJob | Default        | Default     | Default     | 103.287 ns | 0.5251 ns | 0.4655 ns | 18.19 |    0.34 |    5 |         - |          NA |
| LeastConnections.SelectReplica | DefaultJob | Default        | Default     | Default     |  98.964 ns | 0.2303 ns | 0.2042 ns | 17.43 |    0.32 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | DefaultJob | Default        | Default     | Default     |  64.863 ns | 0.1257 ns | 0.1176 ns | 11.43 |    0.21 |    3 |         - |          NA |
|                                |            |                |             |             |            |           |           |       |         |      |           |             |
| RoundRobin.SelectReplica       | ShortRun   | 3              | 1           | 3           |   7.795 ns | 0.2147 ns | 0.0118 ns |  1.00 |    0.00 |    1 |         - |          NA |
| Random.SelectReplica           | ShortRun   | 3              | 1           | 3           |  13.979 ns | 0.1118 ns | 0.0061 ns |  1.79 |    0.00 |    2 |         - |          NA |
| LeastLatency.SelectReplica     | ShortRun   | 3              | 1           | 3           | 103.021 ns | 0.3536 ns | 0.0194 ns | 13.22 |    0.02 |    4 |         - |          NA |
| LeastConnections.SelectReplica | ShortRun   | 3              | 1           | 3           | 100.310 ns | 0.2549 ns | 0.0140 ns | 12.87 |    0.02 |    4 |         - |          NA |
| WeightedRandom.SelectReplica   | ShortRun   | 3              | 1           | 3           |  70.093 ns | 1.5158 ns | 0.0831 ns |  8.99 |    0.01 |    3 |         - |          NA |
